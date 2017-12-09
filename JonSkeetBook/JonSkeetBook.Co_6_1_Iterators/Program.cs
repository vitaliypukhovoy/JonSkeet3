﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace JonSkeetBook.Co_6_1_Iterators
{
    //http://sergeyteplyakov.blogspot.ru/2010/06/c-1.html

    /*
        Итератор - поведенческий паттерн проектирования (один из базовых шаблонов LINQ).
        Цель - упрощение коммуникации между объектами.

        Шаблон проектирования «Итератор» предназначен для последовательного доступа ко всем
        элементам коллекции (агрегата), не раскрывая ее внутренней структуры.

        Позволяет получать доступ ко всем элментам последовательности, не заботясь о
        том, что это за последовательность.
        
        Этот паттерн хорошо подходит для реализации конвейера данных, когда элемент
        данных попадает в конвейер и проходит через множество различных преобразований
        или фильтров, перед выходом с другого конца конвейера.

        В .NET паттерн итератора инкапсулируется набором интерфейсов - IEnumerator и
        IEnumerable, а также их обобщенными эквивалентами.

        Если тип реализует IEnumerable, значит по нему можно пройти с помощью итерации.
        И вызвав метод GetEnumerator() на его экземпляре, можно получить реализацию
        интерфейса IEnemerator, которая представляет собой сам итератор! Об итераторе
        удобно говорить, как о курсоре базы данных, т.е. о позиции внутри
        последовательности.

        Для реализации шаблона итератор в C# не обязательно реализовывать интерфейсы
        IEnumerable и IEnemerator, достаточно реализовать метод GetEnumerator(), который
        вернет сущность, которая содержит свойство Current и метод MoveNext().
        Это имеет свои предпосылки:
        Необходимость в сопоставлении с шаблоном (“match the pattern”) потребовалось
        (необходимость в возможности реализовать паттерн без привязки к интерфейсам)
        разработчикам языка C# 1.0 для того, чтобы реализовать типизированные коллекции
        без использования обобщений (который на тот момент еще не было). Интерфейс
        IEnumerable возвращает object, а это значит, что было бы невозможно реализовать
        эффективный итератор по типизированной коллекции целых чисел, поскольку каждый
        раз при получении элемента коллекции происходила бы упаковка и распаковка текущего
        элемента.

        Итератор может перемещаться только вперед, а с одной последовательностью могут
        работать сразу несколько итераторов.

        Все данные не возвращаются за один шаг - клиент запрашивает по одному элементу
        за раз.
    */

    /*
        Отделение класса итератора от класса коллекции в нашей реализации обусловлено не
        только принципом единственной ответственности (SRP – Single Responsibility Principle),
        но и банальным здравым смыслом. Очевидно, что процесс итерирования физически не связан
        с самой коллекцией, но еще более важным фактором является то, что мы можем использовать
        более одного объекта итератора для разных, независимых операций перебора элементов,
        именно поэтому в нашей реализации метод GetEnumerator всегда возвращает новый объект.

        Хотя, как мы увидим позднее, код, генерируемый компилятором не всегда соблюдает
        подобные принципы. Так, например, если метод «блок итератора» возвращает IEnumerable
        или IEnumerable<T>, то компилятор сгенерирует класс, который будет одновременно и
        «коллекцией» и итератором.
    */

    /*
     * https://habrahabr.ru/post/311094/
        ----------------------------------------------------------------------
        Применять yield целесообразно тогда, когда это повышает читаемость кода. А
        "обработка длинных последовательностей" и так далее — это сценарий для
        применения итераторов вообще, yield тут ни при чем (а без дополнительного
        класса-итератора можно обойтись так же, как это делают в BCL — сделав
        структуру).
        ----------------------------------------------------------------------
        "лучше вы приведите пример кода, который написан с использованием yield и который
        невозможно переписать с использованием linq без потери функциональности."

        … и читабельности. Легко.

        public IEnumerable<Identity> GetIdentitiesForCurrentUser()
        {
          yield return TryAuthenticateViaProviderA(Context);
          yield return TryAuthenticateViaProviderB(Context);
          yield return TryAuthenticateLegacy(Context);
          yield return Identity.Anonymous;
        }
        var identity = GetIdentitiesForCurrentUser().First(u => u != null);

        Ну или любой пример с валидацией через IEnumerable<ValidationError>.

        Собственно, ваша ошибка в том, что вы считаете, что yield как-то заменяет/заменяется
        LINQ, хотя на самом деле, yield нужен тогда, когда вам нужно породить перечисление
        (enumeration) и лень/неэффективно/нечитабельно писать свой IEnumerator.
        ----------------------------------------------------------------------
        lair, INC_R, спасибо что потратили на меня время.

        Я понял что ошибался.
        Реально генераторы не покрываются linq никак.
        В своей работе у меня нет таких кейсов, поэтому я начал думать что yield вообще
        никогда не нужен.

        Linq нужен для трансформаций последовательностей (map,reduce), а вот как ты
        последовательность нагенерируешь — это не ответственность linq, для этого как
        раз yield хорошо подходит.
        ----------------------------------------------------------------------
    */

    /*
        IEnumerable<T> (IEnumerable) VS IEnumerator<T> (IEnumerator)
        Вопрос: Что возвращать из метода, которвый содержит блоки итератора (yield return)?

        Ответ: В случае, если вы создаете свою коллекцию, и реализуете метод GetEnumerator,
        то логичнее вернуть IEnumerator<T> (IEnumerator). В остальных же случаях возвращать
        IEnumerator<T> (IEnumerator) обычно не имеет смысла, если вы конечно специально
        этого не ожидаете. Логичнее вернуть IEnumerable<T> (IEnumerable), тем самым дав
        понять, что этот метод просто возвращает итерируемую последовательность.

        В случае, когда вы возвращаете IEnumerator<T> (IEnumerator), компилятор сгенерирует
        код вложенного класса, который релизует интерфейсы IEnumerator<T>, IEnumerator и
        IDisposable (3 интерфейсов, в случае возврата обобщенной версии и 2 в случае не
        обобщенной), который по сути и является итераторм. А в методе, который содержал
        блоки итератора появится код создания экземпляра этого итератора и его возврат из
        метода.

        В случае же, когда вы реализуете IEnumerable<T> (IEnumerable), компилятор сгенерирует
        код вложенного класса, который реализует интерфейсы IEnumerator<T>, IEnumerator и
        IDisposable, а также IEnumerable<T> и IEnumerable (5 интерфейсов, в случае возврата
        обобщенной версии и 3 в случае не обобщенной). По сути этот класс будет как итерато-
        ром, так и итерируемой коллекцией - 2 в 1. В Методе, который содержал блоки итератора
        появится код создания экземпляра этого итератора/коллекции и его возврат из метода.

        Т.е. если мы ожидем, что результат работы метода является коллекцией, то возвращаем
        IEnumerable<T> (IEnumerable), если итератором, то IEnumerator<T> (IEnumerator). Чаще
        всего встречается первое поведение.
    */

    class Program
    {
        static void Main()
        {
            Part2();
            Console.ReadLine();
        }

        private static void Part1()
        {
            var c = new CircleBuffer<int>(new[] { 1, 2, 3, 4, 5 }, 0);
            foreach (var item in c)
                Console.WriteLine(item);

            Console.WriteLine(); //--------------------------------

            var myCollection = new MyCollection(new object[] { 1, 2, 3, 4, 5 });
            foreach (var item in myCollection)
                Console.WriteLine(item);

            Console.WriteLine(); //--------------------------------

            /*
                Компилятор разворачивает foreach приблизительно в следующую конструкцию.
            */
            var wowCollection = new WowCollection(new int[] { 1, 2, 3, 4, 5 });
            var enumerator = wowCollection.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    int value = enumerator.Current;
                    Console.WriteLine(value);
                }
            }
            finally
            {
                enumerator.Dispose();
            }

            Console.WriteLine(); //--------------------------------

            foreach (var i in wowCollection)
            {
                foreach (var j in wowCollection)
                    Console.Write($" {i}{j} |");
                Console.WriteLine();
            }

            Console.WriteLine(); //--------------------------------

            var wowSharp2Iterable = new WowSharp2Iterable<int>(new int[] { 1, 2, 3, 4, 5 });
            foreach (var item in wowSharp2Iterable)
            {
                Console.WriteLine(item);
            }
        }

        
        private static void Part2()
        {
            //JonSkeetBook.Co_6_1_Iterators.Part2.Example1();
            JonSkeetBook.Co_6_1_Iterators.Part2.Example3();
        }
    }
}
